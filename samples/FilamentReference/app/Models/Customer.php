<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\SoftDeletes;

class Customer extends Model
{
    use SoftDeletes;

    const UPDATED_AT = null;
    const CREATED_AT = null;

    protected $fillable = [
        'name',
        'email',
    ];

    public function orders(): HasMany
    {
        return $this->hasMany(Order::class);
    }
}
